using Studio.ShortSleeve.UnityMetaHaptics.Common;
using Studio.ShortSleeve.UnityMetaHaptics.SDL2;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Test script for SDL2 haptics playback.
/// IMPORTANT: Make sure you have an SDL2HapticsSystem component in your scene!
/// </summary>
public class HapticTest : MonoBehaviour
{
    HapticResponse currentResponse;

    [SerializeField]
    HapticClip clip;

    [SerializeField]
    bool shouldLoop;

    void Start()
    {
        // Verify SDL2HapticsSystem is initialized
        if (SDL2HapticsSystem.Instance == null)
        {
            Debug.LogError(
                "HapticTest: SDL2HapticsSystem not found! Add SDL2HapticsSystem component to scene or call SDL2HapticsSystem.CreateInstance()"
            );
        }
        else if (!SDL2HapticsSystem.Instance.IsInitialized)
        {
            Debug.LogError("HapticTest: SDL2HapticsSystem is not initialized!");
        }
    }

    public void Play()
    {
        if (clip == null)
        {
            Debug.LogWarning("HapticTest: No clip assigned");
            return;
        }

        if (Gamepad.current == null)
        {
            Debug.LogWarning("HapticTest: No gamepad connected");
            return;
        }

        // Play the haptic clip via SDL2HapticsSystem
        currentResponse = SDL2HapticsSystem.Instance.Play(
            new HapticRequest
            {
                Clip = clip,
                GamepadDevice = Gamepad.current,
                ShouldLoop = shouldLoop,
            }
        );
    }

    public void Stop()
    {
        if (currentResponse.ID != -1)
        {
            currentResponse.Stop();
        }
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
