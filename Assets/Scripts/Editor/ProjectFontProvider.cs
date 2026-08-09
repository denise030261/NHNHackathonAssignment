#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace NHNHackathon.EditorTools
{
    public static class ProjectFontProvider
    {
        public const string RegularFontPath =
            "Assets/Phont/MaruBuriTTF/MaruBuri-Regular.ttf";

        public static Font LoadRegular()
        {
            Font font = AssetDatabase.LoadAssetAtPath<Font>(RegularFontPath);
            if (font == null)
            {
                Debug.LogError($"Project font was not found: {RegularFontPath}");
            }
            return font;
        }
    }
}
#endif
