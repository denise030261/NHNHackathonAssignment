#if UNITY_EDITOR
using UnityEditor;

namespace NHNHackathon.EditorTools
{
    public static class DancingAIModelSetup
    {
        [MenuItem("Tools/NHN Hackathon/Characters/Apply Dancing AI Model")]
        public static void Build()
        {
            PlayerCharacterAnimationSetup.ApplyDancingAIModelOnly();
        }
    }
}
#endif
