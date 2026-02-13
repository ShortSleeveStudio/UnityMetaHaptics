// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace Studio.ShortSleeve.UnityMetaHaptics.Common
{
    /// <summary>
    /// Represents an imported haptic clip asset for gamepad playback.
    /// </summary>
    ///
    /// HapticClip contains the data of a haptic clip asset imported from a <c>.haptic</c> file,
    /// in a format suitable for playing it back on gamepads at runtime.
    /// A HapticClip is created by <c>HapticImporter</c> when importing a haptic clip asset
    /// in the Unity editor, and can be played back at runtime with <c>GamepadHapticsPlayer</c>.
    public class HapticClip : ScriptableObject
    {
        /// <summary>
        /// The data model parsed from Meta Haptics Studio .haptic files,
        /// containing amplitude and frequency breakpoint envelopes for gamepad playback.
        /// </summary>
        [SerializeField]
        internal DataModel dataModel;

        /// <summary>
        /// Gets the data model for this haptic clip (read-only).
        /// </summary>
        public DataModel DataModel => dataModel;
    }
}
