// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.IO;
using Studio.ShortSleeve.UnityMetaHaptics.Common;
using UnityEngine;
#if UNITY_2020_2_OR_NEWER
using UnityEditor.AssetImporters;
#elif UNITY_2019_4_OR_NEWER
using UnityEditor.Experimental.AssetImporters;
#endif

namespace Studio.ShortSleeve.UnityMetaHaptics.Editor
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
            try
            {
                // Load and parse .haptic clip from file
                if (!File.Exists(ctx.assetPath))
                {
                    ctx.LogImportError($"Haptic file not found: {ctx.assetPath}");
                    return;
                }

                string jsonString = File.ReadAllText(ctx.assetPath);
                if (string.IsNullOrWhiteSpace(jsonString))
                {
                    ctx.LogImportError($"Haptic file is empty: {ctx.assetPath}");
                    return;
                }

                HapticClip hapticClip = ScriptableObject.CreateInstance<HapticClip>();

                // Convert JSON to DataModel for gamepad playback
                hapticClip.dataModel = JsonUtility.FromJson<DataModel>(jsonString);

                // Validate imported data
                if (
                    hapticClip.DataModel == null
                    || hapticClip.DataModel.signals?.continuous?.envelopes?.amplitude == null
                    || hapticClip.DataModel.signals?.continuous?.envelopes?.frequency == null
                    || hapticClip.DataModel.signals.continuous.envelopes.amplitude.Length == 0
                    || hapticClip.DataModel.signals.continuous.envelopes.frequency.Length == 0
                )
                {
                    ctx.LogImportError(
                        $"Haptic file contains invalid or missing data: {ctx.assetPath}"
                    );
                    return;
                }

                // Use hapticClip as the imported asset
                ctx.AddObjectToAsset("Studio.ShortSleeve.UnityMetaHaptics.HapticClip", hapticClip);
                ctx.SetMainObject(hapticClip);
            }
            catch (IOException ex)
            {
                ctx.LogImportError($"Failed to read haptic file '{ctx.assetPath}': {ex.Message}");
            }
            catch (Exception ex)
            {
                ctx.LogImportError($"Failed to import haptic file '{ctx.assetPath}': {ex.Message}");
            }
        }
    }
}
