using UnityEngine.InputSystem;

namespace Studio.ShortSleeve.UnityMetaHaptics.Common
{
    public struct HapticRequest
    {
        public HapticClip Clip;
        public Gamepad GamepadDevice;
        public bool ShouldLoop;
        public bool UseFixedTime;
        public bool ApplyTimeScale;
    }
}
