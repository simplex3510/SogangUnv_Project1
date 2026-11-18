using System;
using System.Collections;

using UnityEngine;

using Game.Logistics;
using Game.Player.Interaction;
using Game.Player.Movement;
using Game.Systems;

namespace Game.Audio
{
    /// <summary>
    /// 기존 게임 이벤트를 구독해 효과음·배경음을 재생한다. 배정: 사운드 추출 표.xlsx.
    /// 씬에 배치하고 클립은 인스펙터에서 할당한다.
    /// </summary>
    public class GameAudio : MonoBehaviour
    {
        public static GameAudio Instance { get; private set; }

        // 클립마다 바로 아래 볼륨으로 재생한다. 루프는 플레이 중 변경이 즉시, 1회 재생은 다음 재생부터 반영된다
        [Header("Loop Clips")]
        [SerializeField] private AudioClip _bgmClip;
        [SerializeField, Range(0f, 1f)] private float _bgmVolume = 0.4f;
        [SerializeField] private AudioClip _idleClip;
        [SerializeField, Range(0f, 1f)] private float _idleVolume = 0.35f;
        [SerializeField] private AudioClip _accelerateClip;
        [SerializeField, Range(0f, 1f)] private float _accelerateVolume = 0.35f;
        [SerializeField] private AudioClip _driftClip;
        [SerializeField, Range(0f, 1f)] private float _driftVolume = 0.2f;

        [Header("One-Shot Clips")]
        [SerializeField] private AudioClip _crash;
        [SerializeField, Range(0f, 1f)] private float _crashVolume = 0.8f;
        [SerializeField] private AudioClip _load;
        [SerializeField, Range(0f, 1f)] private float _loadVolume = 0.8f;
        [SerializeField] private AudioClip _unload;
        [SerializeField, Range(0f, 1f)] private float _unloadVolume = 0.8f;
        [SerializeField] private AudioClip _groupLoaded;
        [SerializeField, Range(0f, 1f)] private float _groupLoadedVolume = 0.8f;
        [SerializeField] private AudioClip _allUnloaded;
        [SerializeField, Range(0f, 1f)] private float _allUnloadedVolume = 0.8f;
        [SerializeField] private AudioClip _toast;
        [SerializeField, Range(0f, 1f)] private float _toastVolume = 0.8f;
        [SerializeField] private AudioClip _dayStart;
        [SerializeField, Range(0f, 1f)] private float _dayStartVolume = 0.8f;
        [SerializeField] private AudioClip _dayEnd;
        [SerializeField, Range(0f, 1f)] private float _dayEndVolume = 0.8f;
        [SerializeField] private AudioClip _menuOpen;
        [SerializeField, Range(0f, 1f)] private float _menuOpenVolume = 0.8f;
        [SerializeField] private AudioClip _menuClose;
        [SerializeField, Range(0f, 1f)] private float _menuCloseVolume = 0.8f;

        [Header("BGM")]

        [Tooltip("하루 시작 페이드 인·정산 페이드 아웃 길이(초)")]
        [SerializeField] private float _bgmFadeSeconds = 2f;

        [Header("Engine")]
        [Tooltip("이 속도(m/s) 이상이면 이동 루프, 미만이면 정지 루프")]
        [SerializeField] private float _movingSpeedThreshold = 0.3f;
        [SerializeField] private float _accelPitchMin = 0.9f;
        [SerializeField] private float _accelPitchMax = 1.3f;

        private AudioSource _sfx;
        private AudioSource _idle;
        private AudioSource _accelerate;
        private AudioSource _drift;
        private AudioSource _bgm;
        private Coroutine _bgmFade;

        private PlayerMovementController _movement;
        private PlayerInteractionController _interactionController;
        private PlayerCargoImpactController _impact;

        /// <summary>토스트 알림이 뜰 때.</summary>
        public static void PlayToast() => PlayStatic(a => a.PlayOneShot(a._toast, a._toastVolume));

        /// <summary>물류센터 창이 열릴 때.</summary>
        public static void PlayMenuOpen() => PlayStatic(a => a.PlayOneShot(a._menuOpen, a._menuOpenVolume));

        /// <summary>물류센터 창이 닫힐 때.</summary>
        public static void PlayMenuClose() => PlayStatic(a => a.PlayOneShot(a._menuClose, a._menuCloseVolume));

        /// <summary>물류센터 창 기준으로 한 물류 그룹을 모두 실었을 때.</summary>
        public static void PlayGroupLoaded() => PlayStatic(a => a.PlayOneShot(a._groupLoaded, a._groupLoadedVolume));

        private static void PlayStatic(Action<GameAudio> play)
        {
            if (Instance != null)
            {
                play(Instance);
            }
        }

        private void Awake()
        {
            Instance = this;

            _sfx = gameObject.AddComponent<AudioSource>();
            _idle = CreateLoop(_idleClip, _idleVolume);
            _accelerate = CreateLoop(_accelerateClip, _accelerateVolume);
            _drift = CreateLoop(_driftClip, _driftVolume);
            _bgm = CreateLoop(_bgmClip, 0f);
        }

        private void Start()
        {
            _movement = FindAnyObjectByType<PlayerMovementController>();
            _interactionController = FindAnyObjectByType<PlayerInteractionController>();
            _impact = FindAnyObjectByType<PlayerCargoImpactController>();

            if (_impact != null)
            {
                _impact.ImpactOccurred += OnImpact;
            }

            if (_interactionController != null)
            {
                _interactionController.LoadedParcelsChanged += OnLoadedParcelsChanged;
            }

            if (SettlementSystem.Instance != null)
            {
                SettlementSystem.Instance.Settled += OnSettled;
            }

            // 씬 시작이 곧 일과 시작이다(DailyOperationSystem.Start → StartDay)
            PlayOneShot(_dayStart, _dayStartVolume);
            if (_bgm.clip != null)
            {
                _bgm.Play();
                FadeBgm(_bgmVolume);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            if (_impact != null)
            {
                _impact.ImpactOccurred -= OnImpact;
            }

            if (_interactionController != null)
            {
                _interactionController.LoadedParcelsChanged -= OnLoadedParcelsChanged;
            }

            if (SettlementSystem.Instance != null)
            {
                SettlementSystem.Instance.Settled -= OnSettled;
            }
        }

        // 루프 상태는 이벤트가 아니라 프레임 끝 상태로 맞춘다
        private void Update()
        {
            if (_movement == null)
            {
                return;
            }

            bool isOperating = DailyOperationSystem.Instance == null || DailyOperationSystem.Instance.IsOperating;
            float speed = Mathf.Abs(_movement.ForwardSpeed);
            bool isMoving = speed >= _movingSpeedThreshold;

            float maxSpeed = _movement.Config.GetModeSettings(VehicleDrivingMode.Fast).MaxForwardSpeed;
            _accelerate.pitch = Mathf.Lerp(_accelPitchMin, _accelPitchMax, maxSpeed > 0f ? Mathf.Clamp01(speed / maxSpeed) : 0f);

            // 인스펙터에서 바꾼 루프 볼륨을 바로 듣도록 매 프레임 반영한다
            _idle.volume = _idleVolume;
            _accelerate.volume = _accelerateVolume;
            _drift.volume = _driftVolume;

            SetLoop(_idle, isOperating && isMoving == false);
            SetLoop(_accelerate, isOperating && isMoving);
            SetLoop(_drift, isOperating && _movement.IsDrifting);
        }

        private void FadeBgm(float targetVolume)
        {
            if (_bgmFade != null)
            {
                StopCoroutine(_bgmFade);
            }

            _bgmFade = StartCoroutine(FadeBgmRoutine(targetVolume));
        }

        // 정산 화면 중에도 진행되도록 unscaled 시간을 쓴다
        private IEnumerator FadeBgmRoutine(float targetVolume)
        {
            float start = _bgm.volume;
            for (float t = 0f; t < _bgmFadeSeconds; t += Time.unscaledDeltaTime)
            {
                _bgm.volume = Mathf.Lerp(start, targetVolume, t / _bgmFadeSeconds);
                yield return null;
            }

            _bgm.volume = targetVolume;
            if (targetVolume <= 0f)
            {
                _bgm.Stop();
            }

            _bgmFade = null;
        }

        private void OnImpact() => PlayOneShot(_crash, _crashVolume);

        private void OnLoadedParcelsChanged(Parcel parcel)
        {
            if (parcel == null)
            {
                return;
            }

            if (parcel.Status == ParcelStatus.Loaded)
            {
                PlayOneShot(_load, _loadVolume);
            }
            else if (parcel.Status == ParcelStatus.Delivered)
            {
                PlayOneShot(_unload, _unloadVolume);

                // 이 배송지로 지금 배송 가능한 적재 물류가 더 없으면 전체 하차 완료
                var loaded = _interactionController.LoadedParcels;
                bool anyLeft = false;
                for (int i = 0; i < loaded.Count && anyLeft == false; i++)
                {
                    anyLeft = loaded[i].IsDeliverableTo(parcel.Target);
                }

                if (anyLeft == false)
                {
                    PlayOneShot(_allUnloaded, _allUnloadedVolume);
                }
            }
        }

        private void OnSettled(SettlementSystem.SettlementResult _)
        {
            PlayOneShot(_dayEnd, _dayEndVolume);
            FadeBgm(0f);
        }

        private void PlayOneShot(AudioClip clip, float volume)
        {
            if (clip != null)
            {
                _sfx.PlayOneShot(clip, volume);
            }
        }

        private static void SetLoop(AudioSource source, bool shouldPlay)
        {
            if (source.clip == null || source.isPlaying == shouldPlay)
            {
                return;
            }

            if (shouldPlay)
            {
                source.Play();
            }
            else
            {
                source.Stop();
            }
        }

        private AudioSource CreateLoop(AudioClip clip, float volume)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.playOnAwake = false;
            source.volume = volume;
            return source;
        }
    }
}
