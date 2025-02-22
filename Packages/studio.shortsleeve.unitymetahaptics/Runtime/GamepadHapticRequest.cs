using UnityEngine.InputSystem;

namespace Studio.ShortSleeve.UnityMetaHaptics
{
    public struct GamepadHapticRequest
    {
        public GamepadRumble Clip;
        public Gamepad Gamepad;
        public bool ShouldLoop;
        public bool UseFixedTime;
        public bool ApplyTimeScale;
    }
}
