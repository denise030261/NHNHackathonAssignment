#if UNITY_EDITOR
using UnityEditor;

namespace NHNHackathon.EditorTools
{
    public static class PlayerDanceAnimationSetup
    {
        [MenuItem("Tools/NHN Hackathon/Characters/Apply Player Dance Animations")]
        public static void Build()
        {
            PlayerCharacterAnimationSetup.Build();
        }
    }
}
#endif
