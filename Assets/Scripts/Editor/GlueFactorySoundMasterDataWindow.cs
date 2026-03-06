#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

public sealed class GlueFactorySoundMasterDataWindow : EditorWindow
{
    private GlueFactoryAudioConfig config;
    private SerializedObject serializedConfig;
    private SerializedProperty soundsProperty;
    private ReorderableList soundsList;
    private Vector2 scroll;

    [MenuItem("GlueFactory/Open Sound Master Data")]
    public static void OpenWindow()
    {
        var window = GetWindow<GlueFactorySoundMasterDataWindow>("Sound Master Data");
        window.minSize = new Vector2(760, 430);
        window.RefreshBinding();
        window.Show();
    }

    private void OnEnable()
    {
        RefreshBinding();
    }

    private void RefreshBinding()
    {
        config = FindOrCreateConfig();
        if (config == null)
        {
            serializedConfig = null;
            soundsProperty = null;
            soundsList = null;
            return;
        }

        serializedConfig = new SerializedObject(config);
        soundsProperty = serializedConfig.FindProperty("sounds");
        BuildList();
    }

    private void OnGUI()
    {
        DrawHeader();
        if (config == null || serializedConfig == null || soundsProperty == null)
        {
            EditorGUILayout.HelpBox("No GlueFactoryAudio config found.", MessageType.Warning);
            if (GUILayout.Button("Generate Editable Default Sound Config", GUILayout.Height(28)))
            {
                GlueFactoryAudioConfigTools.GenerateEditableDefaultSoundConfig();
                RefreshBinding();
            }
            return;
        }

        serializedConfig.Update();
        DrawGlobalSettings();
        DrawToolbar();

        scroll = EditorGUILayout.BeginScrollView(scroll);
        soundsList?.DoLayoutList();
        EditorGUILayout.EndScrollView();

        serializedConfig.ApplyModifiedProperties();
    }

    private void DrawHeader()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Glue Factory Sound Master Data", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Source", config == null ? "<None>" : config.name);
        EditorGUILayout.EndVertical();
    }

    private void DrawGlobalSettings()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.PropertyField(serializedConfig.FindProperty("enableSounds"), new GUIContent("Enable Sounds"));
        EditorGUILayout.PropertyField(serializedConfig.FindProperty("masterVolume"), new GUIContent("Master Volume"));
        EditorGUILayout.PropertyField(serializedConfig.FindProperty("sfxVolume"), new GUIContent("SFX Volume"));
        EditorGUILayout.PropertyField(serializedConfig.FindProperty("musicVolume"), new GUIContent("Music Volume"));
        EditorGUILayout.PropertyField(serializedConfig.FindProperty("backgroundMusic"), new GUIContent("Background Music"));
        EditorGUILayout.PropertyField(serializedConfig.FindProperty("playMusicOnStart"), new GUIContent("Play Music On Start"));
        EditorGUILayout.PropertyField(serializedConfig.FindProperty("loopMusic"), new GUIContent("Loop Music"));
        EditorGUILayout.PropertyField(serializedConfig.FindProperty("sfxSourcePoolSize"), new GUIContent("SFX Source Pool Size"));
        EditorGUILayout.EndVertical();
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Ensure Defaults", GUILayout.Height(26)))
        {
            Undo.RecordObject(config, "Ensure Default Sounds");
            config.EnsureDefaults();
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            RefreshBinding();
        }
        if (GUILayout.Button("Apply To Running Game", GUILayout.Height(26)))
        {
            ApplyToRunningGame();
        }
        if (GUILayout.Button("Ping Asset", GUILayout.Height(26)))
        {
            Selection.activeObject = config;
            EditorGUIUtility.PingObject(config);
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(6);
    }

    private void BuildList()
    {
        soundsList = new ReorderableList(serializedConfig, soundsProperty, true, true, true, true);
        soundsList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Sound Entries");
        soundsList.elementHeight = 134f;
        soundsList.drawElementCallback = (rect, index, _, _) =>
        {
            var item = soundsProperty.GetArrayElementAtIndex(index);
            if (item == null)
            {
                return;
            }

            rect.y += 2f;
            var line = new Rect(rect.x, rect.y, rect.width, 18f);
            EditorGUI.PropertyField(line, item.FindPropertyRelative("id"), new GUIContent("Id"));
            line.y += 20f;
            EditorGUI.PropertyField(line, item.FindPropertyRelative("clip"), new GUIContent("Clip"));
            line.y += 20f;
            EditorGUI.PropertyField(line, item.FindPropertyRelative("volume"), new GUIContent("Volume"));
            line.y += 20f;
            EditorGUI.PropertyField(line, item.FindPropertyRelative("pitchRange"), new GUIContent("Pitch Range"));
            line.y += 20f;
            EditorGUI.PropertyField(line, item.FindPropertyRelative("cooldownSeconds"), new GUIContent("Cooldown (sec)"));
        };
    }

    private void ApplyToRunningGame()
    {
        serializedConfig.ApplyModifiedProperties();
        var manager = FindFirstObjectByType<GlueFactoryAudioManager>();
        var game = FindFirstObjectByType<GlueFactoryGameManager>();
        if (manager == null || game == null)
        {
            Debug.LogWarning("Apply failed: need GlueFactoryAudioManager and GlueFactoryGameManager in scene.");
            return;
        }

        manager.Configure(config, game);
        Debug.Log("Applied sound master data to running game.");
    }

    private static GlueFactoryAudioConfig FindOrCreateConfig()
    {
        const string resourcesFolder = "Assets/Resources";
        const string assetPath = "Assets/Resources/GlueFactoryAudio.asset";

        if (!AssetDatabase.IsValidFolder(resourcesFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        var cfg = AssetDatabase.LoadAssetAtPath<GlueFactoryAudioConfig>(assetPath);
        if (cfg == null)
        {
            cfg = ScriptableObject.CreateInstance<GlueFactoryAudioConfig>();
            cfg.EnsureDefaults();
            AssetDatabase.CreateAsset(cfg, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        else
        {
            cfg.EnsureDefaults();
            EditorUtility.SetDirty(cfg);
        }

        return cfg;
    }
}
#endif

