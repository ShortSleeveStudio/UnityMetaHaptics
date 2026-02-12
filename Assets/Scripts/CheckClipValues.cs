using Studio.ShortSleeve.UnityMetaHaptics.Common;
using UnityEngine;

public class CheckClipValues : MonoBehaviour
{
    public HapticClip clip;

    void Start()
    {
        if (clip != null && clip.FrameCount > 0)
        {
            var frame0 = clip.GetFrameAt(0);
            Debug.Log(
                $"Clip '{clip.name}' Frame 0: low={frame0.lowFreqAmp:F3}, high={frame0.highFreqAmp:F3}"
            );

            // For kick with equal-power: should be ~0.640, 0.768
            // For kick with linear: should be ~0.410, 0.590
        }
    }
}
