namespace Studio.ShortSleeve.UnityMetaHaptics.Common
{
    public interface IHapticsPlayer
    {
        public void Stop(HapticResponse response);
        public bool IsValid(HapticResponse response);
    }
}
