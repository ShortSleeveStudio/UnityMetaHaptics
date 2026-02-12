using System;
using UnityEngine;

namespace Studio.ShortSleeve.UnityMetaHaptics.Common
{
    /// <summary>
    /// Represents a rumble keyframe at 25 FPS.
    /// </summary>
    [Serializable]
    public struct RumbleKeyframe
    {
        public float lowFreqAmp;   // 0.0-1.0 (large motor)
        public float highFreqAmp;  // 0.0-1.0 (small motor)
    }

    /// <summary>
    /// Represents an imported haptic clip asset.
    /// Contains 25 FPS rumble keyframes for gamepad playback.
    /// 25 FPS prevents Bluetooth buffer saturation on controllers like Switch Pro.
    /// </summary>
    [CreateAssetMenu(fileName = "NewHapticClip", menuName = "Haptics/Haptic Clip")]
    public class HapticClip : ScriptableObject
    {
        /// <summary>
        /// Sample rate for keyframes (25 FPS prevents Bluetooth buffer saturation).
        /// </summary>
        public const int SAMPLE_RATE = 25;

        /// <summary>
        /// Rumble keyframes sampled at 25 FPS.
        /// </summary>
        [SerializeField]
        RumbleKeyframe[] keyframes;

        /// <summary>
        /// Duration of the clip in seconds.
        /// </summary>
        [SerializeField]
        float duration;

        /// <summary>
        /// Number of keyframes in this clip.
        /// </summary>
        public int FrameCount => keyframes?.Length ?? 0;

        /// <summary>
        /// Duration of the clip in seconds.
        /// </summary>
        public float Duration => duration;

        /// <summary>
        /// Gets the keyframe at the specified index.
        /// </summary>
        public RumbleKeyframe GetFrameAt(int index)
        {
            if (keyframes == null || index < 0 || index >= keyframes.Length)
                return new RumbleKeyframe { lowFreqAmp = 0, highFreqAmp = 0 };

            return keyframes[index];
        }

        /// <summary>
        /// Sets the keyframes for this clip (used by importer).
        /// </summary>
        public void SetKeyframes(RumbleKeyframe[] frames, float clipDuration)
        {
            keyframes = frames;
            duration = clipDuration;
        }
    }
}
