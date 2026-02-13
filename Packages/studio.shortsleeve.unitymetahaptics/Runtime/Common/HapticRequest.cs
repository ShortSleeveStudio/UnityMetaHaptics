using UnityEngine.InputSystem;

namespace Studio.ShortSleeve.UnityMetaHaptics.Common
{
    /// <summary>
    /// Defines the parameters for playing a haptic clip on a gamepad.
    /// </summary>
    public struct HapticRequest
    {
        /// <summary>
        /// The haptic clip to play.
        /// </summary>
        public HapticClip Clip;

        /// <summary>
        /// The target gamepad device for haptic playback.
        /// </summary>
        public Gamepad GamepadDevice;

        /// <summary>
        /// If true, the haptic clip will loop indefinitely until stopped.
        /// </summary>
        public bool ShouldLoop;

        /// <summary>
        /// If true, uses FixedUpdate timing instead of Update timing for more consistent physics-aligned playback.
        /// </summary>
        public bool UseFixedTime;

        /// <summary>
        /// If true, haptic intensity will be scaled by Time.timeScale (affected by slow motion/pause).
        /// If false, haptics will play at full intensity regardless of timeScale.
        /// </summary>
        public bool ApplyTimeScale;

        /// <summary>
        /// Determines how amplitude is distributed between low and high frequency motors.
        /// EqualPower (recommended) maintains constant perceived intensity during crossfade.
        /// Linear is simpler but can cause intensity dip at 50/50 blend.
        /// </summary>
        public CrossfadeMode CrossfadeMode;
    }
}
