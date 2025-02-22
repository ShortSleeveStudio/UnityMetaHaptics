using Studio.ShortSleeve.UnityMetaHaptics;
using UnityEngine;
using UnityEngine.InputSystem;

public class HapticTest : MonoBehaviour
{
    [SerializeField]
    GamepadHaptics haptics;

    [SerializeField]
    HapticClip clip;

    void Awake()
    {
        GamepadHapticResponse response = haptics.Play(
            new()
            {
                Clip = clip.gamepadRumble,
                Gamepad = Gamepad.current,
                ShouldLoop = true,
                UseFixedTime = true,
                ApplyTimeScale = true,
            }
        );
        Await(response);
    }

    public async void Await(GamepadHapticResponse test)
    {
        await Awaitable.WaitForSecondsAsync(5f);
        test.Stop();
    }
}
