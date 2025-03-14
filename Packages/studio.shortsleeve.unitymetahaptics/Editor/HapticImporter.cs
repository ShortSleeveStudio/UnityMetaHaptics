// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.IO;
using Studio.ShortSleeve.UnityMetaHaptics.Common;
using UnityEngine;
#if UNITY_2020_2_OR_NEWER
using UnityEditor.AssetImporters;
#elif UNITY_2019_4_OR_NEWER
using UnityEditor.Experimental.AssetImporters;
#endif

namespace Lofelt.NiceVibrations
{
    [ScriptedImporter(version: 3, ext: "haptic", AllowCaching = true)]
    /// <summary>
    /// Provides an importer for the HapticClip component.
    /// </summary>
    ///
    /// The importer takes a <c>.haptic</c> file and converts it into a HapticClip.
    public class HapticImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            // Load .haptic clip from file
            byte[] jsonBytes = File.ReadAllBytes(ctx.assetPath);
            HapticClip hapticClip = ScriptableObject.CreateInstance<HapticClip>();
            hapticClip.json = jsonBytes;

            // Step 1: Convert bytes to DataModel
            hapticClip.dataModel = JsonUtility.FromJson<DataModel>(
                System.Text.Encoding.UTF8.GetString(jsonBytes)
            );

            // Step 3: Add emphasis to breakpoints
            //         As far as I understand, this does nothing if there are no parameters (just as there are no parameters in the original code)
            //         so we don't do it.
            // hapticClip.dataModel.signals.continuous.envelopes.amplitude =
            //     Studio.ShortSleeve.UnityMetaHaptics.Editor.Emphasizer.EmphasizeAmplitudeBreakpoints(
            //         default,
            //         hapticClip.dataModel.signals.continuous.envelopes.amplitude
            //     );

            // Use hapticClip as the imported asset
            ctx.AddObjectToAsset("Studio.ShortSleeve.UnityMetaHaptics.HapticClip", hapticClip);
            ctx.SetMainObject(hapticClip);
        }
    }
}
