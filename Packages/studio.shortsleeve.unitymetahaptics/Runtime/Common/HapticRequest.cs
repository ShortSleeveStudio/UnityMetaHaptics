namespace Studio.ShortSleeve.UnityMetaHaptics.Common
{
    public struct HapticRequest<T>
    {
        public HapticClip Clip;
        public T Device;
        public bool ShouldLoop;
        public bool UseFixedTime;
        public bool ApplyTimeScale;
    }
}
