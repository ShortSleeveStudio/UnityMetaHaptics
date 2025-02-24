namespace Studio.ShortSleeve.UnityMetaHaptics.Common
{
    public interface IHapticsPlayer<T>
    {
        public void Stop(HapticResponse<T> response);
        public bool IsValid(HapticResponse<T> response);
    }
}
