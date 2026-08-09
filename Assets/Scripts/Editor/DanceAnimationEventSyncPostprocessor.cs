#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;

namespace NHNHackathon.EditorTools
{
    public sealed class DanceAnimationEventSyncPostprocessor : AssetPostprocessor
    {
        private static readonly string[] SourcePaths =
        {
            "Assets/Art/Animations/Dance1.anim",
            "Assets/Art/Animations/Dance2.anim",
            "Assets/Art/Animations/Dance3.anim",
            "Assets/Art/Animations/Dance4.anim"
        };

        private static bool syncScheduled;

        private static void OnPostprocessAllAssets(
            string[] importedAssets, string[] deletedAssets,
            string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (syncScheduled || !importedAssets.Any(imported =>
                    SourcePaths.Any(source => string.Equals(
                        imported, source, StringComparison.OrdinalIgnoreCase))))
            {
                return;
            }

            syncScheduled = true;
            EditorApplication.delayCall += () =>
            {
                syncScheduled = false;
                PlayerCharacterAnimationSetup.SyncDanceAnimationEvents();
            };
        }
    }
}
#endif
