namespace Studio.ShortSleeve.UnityMetaHaptics.Common
{
    public interface IHapticsPlayer<T>
    {
        public void Stop(GamepadHapticResponse<T> response);
        public bool IsValid(GamepadHapticResponse<T> response);
    }
}
