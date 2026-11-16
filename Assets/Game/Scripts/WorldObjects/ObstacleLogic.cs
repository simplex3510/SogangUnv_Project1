using UnityEngine;

namespace Game.WorldObjects
{
    /// <summary>오브젝트 하나가 한 번만 실행되도록 막는 순수 판정(방해물 프레임워크 R3, R6).</summary>
    public sealed class OneShotGate
    {
        public bool IsUsed { get; private set; }

        /// <summary>처음 호출만 true. 동시에 두 번 접촉해도 첫 접촉만 실행된다.</summary>
        public bool TryUse()
        {
            if (IsUsed)
            {
                return false;
            }

            IsUsed = true;
            return true;
        }
    }

    /// <summary>
    /// 최고 속도 배율 효과의 중첩 상태(방해물 프레임워크 R3). 순수 C#이라 MonoBehaviour 없이 판정한다.
    /// 겹치면 1에서 가장 먼 값 하나만 적용하고, 지속 시간은 새 요청 값으로 갱신한다.
    /// </summary>
    public sealed class SpeedEffectState
    {
        private float _multiplier = 1f;
        private float _remainingSeconds;

        public bool IsActive => _remainingSeconds > 0f;

        /// <summary>현재 적용 중인 배율. 효과가 없으면 1.</summary>
        public float Multiplier => IsActive ? _multiplier : 1f;

        public void Apply(float multiplier, float durationSeconds)
        {
            if (durationSeconds <= 0f)
            {
                return;
            }

            // 같은 세기면 새 요청을 따른다(방향이 반대인 경우 포함)
            if (IsActive == false || Mathf.Abs(multiplier - 1f) >= Mathf.Abs(_multiplier - 1f))
            {
                _multiplier = multiplier;
            }

            _remainingSeconds = durationSeconds;
        }

        public void Tick(float deltaSeconds)
        {
            if (_remainingSeconds > 0f)
            {
                _remainingSeconds -= deltaSeconds;
            }
        }

        /// <summary>하루 종료 시 지속 중인 효과를 해제한다(R6).</summary>
        public void Clear()
        {
            _remainingSeconds = 0f;
            _multiplier = 1f;
        }
    }
}
