using System.IO;
using Studio.ShortSleeve.UnityMetaHaptics.Common;
using Studio.ShortSleeve.UnityMetaHaptics.Editor;
using UnityEngine;
#if UNITY_2020_2_OR_NEWER
using UnityEditor.AssetImporters;
#elif UNITY_2019_4_OR_NEWER
using UnityEditor.Experimental.AssetImporters;
#endif

namespace Studio.ShortSleeve.UnityMetaHaptics.Editor
{
    /// <summary>
    /// Crossfade mode for mapping frequency to rumble motors.
    /// </summary>
    public enum CrossfadeMode
    {
        /// <summary>
        /// Linear crossfade: splits amplitude proportionally between motors.
        /// More directional feel, but loses intensity when both motors active.
        /// </summary>
        Linear,

        /// <summary>
        /// Equal-power crossfade: preserves total perceived intensity.
        /// Uses square root to maintain constant energy across crossfade.
        /// </summary>
        EqualPower,
    }

    [ScriptedImporter(version: 10, ext: "haptic", AllowCaching = true)]
    /// <summary>
    /// Provides an importer for the HapticClip component.
    /// </summary>
    ///
    /// The importer takes a <c>.haptic</c> file and converts it into a HapticClip
    /// with 25 FPS rumble keyframes for SDL2 GameController playback.
    /// 25 FPS prevents Bluetooth buffer saturation on controllers like Switch Pro.
    public class HapticImporter : ScriptedImporter
    {
        [Tooltip("Crossfade algorithm for mapping frequency to motors")]
        public CrossfadeMode crossfadeMode = CrossfadeMode.EqualPower;

        public override void OnImportAsset(AssetImportContext ctx)
        {
            // Load .haptic clip from file
            byte[] jsonBytes = File.ReadAllBytes(ctx.assetPath);

            // Parse DataModel from JSON
            DataModel dataModel = JsonUtility.FromJson<DataModel>(
                System.Text.Encoding.UTF8.GetString(jsonBytes)
            );

            // Bake emphasis into amplitude breakpoints if present
            if (ShouldBakeEmphasis(dataModel))
            {
                dataModel.signals.continuous.envelopes.amplitude =
                    Emphasizer.EmphasizeAmplitudeBreakpoints(
                        GetEmphasisParameters(),
                        dataModel.signals.continuous.envelopes.amplitude
                    );
            }

            // Get duration
            float duration =
                dataModel.signals.continuous.envelopes.amplitude.Length > 0
                    ? (float)dataModel.signals.continuous.envelopes.amplitude[^1].time
                    : 0f;

            // Calculate number of frames at 25 FPS
            int frameCount = Mathf.CeilToInt(duration * HapticClip.SAMPLE_RATE);
            RumbleKeyframe[] keyframes = new RumbleKeyframe[frameCount];

            // Sample amplitude/frequency at 25 FPS
            for (int i = 0; i < frameCount; i++)
            {
                float time = i / (float)HapticClip.SAMPLE_RATE;

                // Interpolate amplitude and frequency
                float amplitude = InterpolateAmplitude(
                    dataModel.signals.continuous.envelopes.amplitude,
                    time
                );
                float frequency = InterpolateFrequency(
                    dataModel.signals.continuous.envelopes.frequency,
                    time
                );

                // Map to motors based on selected crossfade mode
                float lowFreqAmp,
                    highFreqAmp;

                if (crossfadeMode == CrossfadeMode.EqualPower)
                {
                    // Equal-power crossfade: preserves perceived intensity
                    // Square root compensates for power being proportional to amplitude squared
                    lowFreqAmp = amplitude * Mathf.Sqrt(1.0f - frequency);
                    highFreqAmp = amplitude * Mathf.Sqrt(frequency);
                }
                else // Linear
                {
                    // Linear crossfade: more directional, but loses intensity when both motors active
                    lowFreqAmp = amplitude * (1.0f - frequency);
                    highFreqAmp = amplitude * frequency;
                }

                keyframes[i] = new RumbleKeyframe
                {
                    lowFreqAmp = lowFreqAmp,
                    highFreqAmp = highFreqAmp,
                };
            }

            // Create HapticClip
            HapticClip hapticClip = ScriptableObject.CreateInstance<HapticClip>();
            hapticClip.SetKeyframes(keyframes, duration);

            // Use hapticClip as the imported asset
            ctx.AddObjectToAsset("Studio.ShortSleeve.UnityMetaHaptics.HapticClip", hapticClip);
            ctx.SetMainObject(hapticClip);
        }

        float InterpolateAmplitude(DataModel.AmplitudeBreakpoint[] breakpoints, float time)
        {
            if (breakpoints == null || breakpoints.Length == 0)
                return 0f;

            if (breakpoints.Length == 1)
                return (float)breakpoints[0].amplitude;

            if (time <= breakpoints[0].time)
                return (float)breakpoints[0].amplitude;

            if (time >= breakpoints[^1].time)
                return (float)breakpoints[^1].amplitude;

            // Find the surrounding breakpoints
            for (int i = 0; i < breakpoints.Length - 1; i++)
            {
                if (time < breakpoints[i + 1].time)
                {
                    // Interpolate between breakpoints[i] and breakpoints[i + 1]
                    float t = (float)(
                        (time - breakpoints[i].time)
                        / (breakpoints[i + 1].time - breakpoints[i].time)
                    );
                    t = Mathf.Clamp01(t); // Ensure t is in valid range
                    return Mathf.Lerp(
                        (float)breakpoints[i].amplitude,
                        (float)breakpoints[i + 1].amplitude,
                        t
                    );
                }
            }

            // Fallback: return last breakpoint value
            return (float)breakpoints[^1].amplitude;
        }

        float InterpolateFrequency(DataModel.FrequencyBreakpoint[] breakpoints, float time)
        {
            if (breakpoints == null || breakpoints.Length == 0)
                return 0.5f; // Default to mid-range if no frequency data

            if (breakpoints.Length == 1)
                return (float)breakpoints[0].frequency;

            if (time <= breakpoints[0].time)
                return (float)breakpoints[0].frequency;

            if (time >= breakpoints[^1].time)
                return (float)breakpoints[^1].frequency;

            // Find the surrounding breakpoints
            for (int i = 0; i < breakpoints.Length - 1; i++)
            {
                if (time < breakpoints[i + 1].time)
                {
                    // Interpolate between breakpoints[i] and breakpoints[i + 1]
                    float t = (float)(
                        (time - breakpoints[i].time)
                        / (breakpoints[i + 1].time - breakpoints[i].time)
                    );
                    t = Mathf.Clamp01(t); // Ensure t is in valid range
                    return Mathf.Lerp(
                        (float)breakpoints[i].frequency,
                        (float)breakpoints[i + 1].frequency,
                        t
                    );
                }
            }

            // Fallback: return last breakpoint value
            return (float)breakpoints[^1].frequency;
        }

        bool ShouldBakeEmphasis(DataModel model)
        {
            DataModel.AmplitudeBreakpoint[] amplitudeBreakpoints = model
                .signals
                .continuous
                .envelopes
                .amplitude;
            for (int i = 0; i < amplitudeBreakpoints.Length; i++)
            {
                if (amplitudeBreakpoints[i].emphasis != null)
                    return true;
            }
            return false;
        }

        EmphasisParameters GetEmphasisParameters()
        {
            return new EmphasisParameters
            {
                DuckingAmplitude = 1.0, // No ducking for gamepad rumble (was 0.0)
                EmphasisLengthSeconds = 0.03,
                DuckingBeforeDurationSeconds = 0.03,
                DuckingAfterDurationSeconds = 0.03,
            };
        }
    }
}
