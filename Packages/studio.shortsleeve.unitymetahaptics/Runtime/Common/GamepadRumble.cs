using System;
using UnityEngine;

namespace Studio.ShortSleeve.UnityMetaHaptics.Common
{
    /// <summary>
    /// Contains a vibration pattern to make a gamepad rumble.
    /// </summary>
    ///
    /// GamepadRumble contains the information on when to set what motor speeds on a gamepad
    /// to make it rumble with a specific pattern.
    ///
    /// GamepadRumble has three arrays of the same length representing the rumble pattern. The
    /// entries for each array index describe for how long to turn on the gamepad's vibration
    /// motors, at what speed.
    [Serializable]
    public struct GamepadRumble
    {
        /// <summary>
        /// The duration, in milliseconds, that the motors will be turned on at the speed set
        /// in \ref lowFrequencyMotorSpeeds and \ref highFrequencyMotorSpeeds at the same array
        /// index
        /// </summary>
        [SerializeField]
        public int[] durationsMs;

        /// <summary>
        /// The total duration of the GamepadRumble, in milliseconds
        /// </summary>
        [SerializeField]
        public int totalDurationMs;

        /// <summary>
        /// The amplitude of the vibration
        /// </summary>
        [SerializeField]
        public float[] amplitude;

        /// <summary>
        /// The frequency of the vibration
        /// </summary>
        [SerializeField]
        public float[] frequency;

        /// <summary>
        /// Checks if the GamepadRumble is valid and also not empty
        /// </summary>
        /// <returns>Whether the GamepadRumble is valid</returns>
        public bool IsValid()
        {
            return durationsMs != null
                && amplitude != null
                && frequency != null
                && durationsMs.Length == amplitude.Length
                && durationsMs.Length == frequency.Length
                && durationsMs.Length > 0;
        }
    }
}
