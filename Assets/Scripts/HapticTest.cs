using Studio.ShortSleeve.UnityMetaHaptics;
using UnityEngine;
using UnityEngine.InputSystem;

public class HapticTest : MonoBehaviour
{
    [SerializeField]
    GamepadHaptics haptics;

    [SerializeField]
    HapticClip clip;

    [SerializeField]
    bool shouldLoop;

    [SerializeField]
    bool useFixedTime = true;

    [SerializeField]
    bool applyTimeScale = false;

    public void Play()
    {
        // This is awaitable
        haptics.Play(
            new()
            {
                Clip = clip,
                Gamepad = Gamepad.current,
                ShouldLoop = shouldLoop,
                UseFixedTime = useFixedTime,
                ApplyTimeScale = applyTimeScale,
            }
        );
    }

    public void Stop()
    {
        haptics.StopAll();
    }
}

#if UNITY_EDITOR
[UnityEditor.CustomEditor(typeof(HapticTest))]
public class LevelScriptEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        HapticTest myTarget = (HapticTest)target;
        DrawDefaultInspector();
        if (GUILayout.Button("Play") && Application.isPlaying)
            myTarget.Play();
        if (GUILayout.Button("Stop") && Application.isPlaying)
            myTarget.Stop();
    }
}
#endif
