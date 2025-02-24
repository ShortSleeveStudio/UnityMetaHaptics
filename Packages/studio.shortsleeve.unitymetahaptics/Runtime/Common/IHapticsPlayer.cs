namespace Studio.ShortSleeve.UnityMetaHaptics.Common
{
    public interface IHapticsPlayer<T>
    {
        public void Stop(GamepadHaptic<T> response);
        public bool IsValid(GamepadHaptic<T> response);
    }
}
