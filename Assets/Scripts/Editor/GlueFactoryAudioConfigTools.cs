#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class GlueFactoryAudioConfigTools
{
    [MenuItem("GlueFactory/Generate Editable Default Sound Config")]
    public static void GenerateEditableDefaultSoundConfig()
    {
        const string resourcesFolder = "Assets/Resources";
        const string assetPath = "Assets/Resources/GlueFactoryAudio.asset";

        if (!AssetDatabase.IsValidFolder(resourcesFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        var config = AssetDatabase.LoadAssetAtPath<GlueFactoryAudioConfig>(assetPath);
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<GlueFactoryAudioConfig>();
            config.EnsureDefaults();
            AssetDatabase.CreateAsset(config, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        else
        {
            Undo.RecordObject(config, "Ensure Default Sound Config");
            config.EnsureDefaults();
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
        }

        Selection.activeObject = config;
        Debug.Log("Generated/ensured editable GlueFactoryAudio config at Assets/Resources/GlueFactoryAudio.asset");
    }
}
#endif

