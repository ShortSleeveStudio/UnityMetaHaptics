namespace Studio.ShortSleeve.UnityMetaHaptics.Common
{
    /// <summary>
    /// Defines how amplitude is distributed between low and high frequency motors during crossfading.
    /// </summary>
    public enum CrossfadeMode
    {
        /// <summary>
        /// Linear crossfade where motor speeds sum to the amplitude value.
        /// Simple but can cause perceived intensity dip at 50/50 blend.
        /// </summary>
        Linear,

        /// <summary>
        /// Equal-power crossfade using square root to maintain constant perceived intensity.
        /// Recommended for most use cases to avoid intensity dip during crossfade.
        /// </summary>
        EqualPower
    }
}
