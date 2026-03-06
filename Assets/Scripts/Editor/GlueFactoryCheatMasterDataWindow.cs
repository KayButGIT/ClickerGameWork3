#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class GlueFactoryCheatMasterDataWindow : EditorWindow
{
    private GlueFactoryCheatDefinition definition;
    private SerializedObject serializedDefinition;
    private int tabIndex;
    private static readonly string[] Tabs = { "General Configuration", "Button Config" };

    [MenuItem("GlueFactory/Open General Configuration")]
    public static void OpenWindow()
    {
        var window = GetWindow<GlueFactoryCheatMasterDataWindow>("General Configuration");
        window.minSize = new Vector2(520f, 280f);
        window.RefreshBinding();
        window.Show();
    }

    private void OnEnable()
    {
        RefreshBinding();
    }

    private void RefreshBinding()
    {
        definition = FindDefinition();
        serializedDefinition = definition == null ? null : new SerializedObject(definition);
    }

    private void OnGUI()
    {
        DrawHeader();
        if (definition == null || serializedDefinition == null)
        {
            EditorGUILayout.HelpBox("No GlueFactoryCheatDefinition found. Generate defaults first.", MessageType.Warning);
            if (GUILayout.Button("Generate Editable Default Cheat Data", GUILayout.Height(28)))
            {
                GlueFactoryCheatDataTools.GenerateEditableDefaultCheatData();
                RefreshBinding();
            }

            return;
        }

        serializedDefinition.Update();

        tabIndex = GUILayout.Toolbar(tabIndex, Tabs);
        EditorGUILayout.Space(8f);

        EditorGUILayout.BeginVertical("box");
        if (tabIndex == 0)
        {
            EditorGUILayout.PropertyField(serializedDefinition.FindProperty("enableCheatButton"), new GUIContent("Enable Cheat"));
            EditorGUILayout.PropertyField(serializedDefinition.FindProperty("showToastOnApply"), new GUIContent("Show Toast On Apply"));
            EditorGUILayout.PropertyField(serializedDefinition.FindProperty("defaultTypedAmount"), new GUIContent("Default Amount In Dialog"));
        }
        else
        {
            EditorGUILayout.PropertyField(serializedDefinition.FindProperty("showCheatButton"), new GUIContent("Show CHEAT Button"));
            EditorGUILayout.PropertyField(serializedDefinition.FindProperty("showSaveButton"), new GUIContent("Show SAVE Button"));
            EditorGUILayout.PropertyField(serializedDefinition.FindProperty("showResetButton"), new GUIContent("Show RESET Button"));
            EditorGUILayout.PropertyField(serializedDefinition.FindProperty("showExitButton"), new GUIContent("Show EXIT Button"));
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Ensure Defaults", GUILayout.Height(26)))
        {
            Undo.RecordObject(definition, "Ensure Cheat Defaults");
            definition.EnsureDefaults();
            EditorUtility.SetDirty(definition);
            MarkDirty();
            RefreshBinding();
        }

        if (GUILayout.Button("Apply To Running Game", GUILayout.Height(26)))
        {
            ApplyToRunningGame();
        }
        EditorGUILayout.EndHorizontal();

        serializedDefinition.ApplyModifiedProperties();
    }

    private void DrawHeader()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Glue Factory General Configuration", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Source", definition == null ? "<None>" : definition.name);
        EditorGUILayout.LabelField("Cheat dialog typed amount and add money.", EditorStyles.miniLabel);
        EditorGUILayout.EndVertical();
    }

    private static GlueFactoryCheatDefinition FindDefinition()
    {
        var managers = GameObject.Find("GlueFactoryManagers");
        if (managers != null)
        {
            var fromManagers = managers.GetComponentInChildren<GlueFactoryCheatDefinition>(true);
            if (fromManagers != null)
            {
                return fromManagers;
            }
        }

        var root = GameObject.Find("GlueCheatDefaults");
        if (root != null)
        {
            return root.GetComponent<GlueFactoryCheatDefinition>();
        }

        return FindFirstObjectByType<GlueFactoryCheatDefinition>(FindObjectsInactive.Include);
    }

    private void ApplyToRunningGame()
    {
        serializedDefinition.ApplyModifiedProperties();
        definition.EnsureDefaults();

        if (!Application.isPlaying)
        {
            Debug.Log("Cheat master data updated.");
            return;
        }

        var game = FindFirstObjectByType<GlueFactoryGameManager>();
        var ui = FindFirstObjectByType<GlueFactorySceneUIManager>();
        if (game != null && ui != null)
        {
            ui.Bind(game);
        }

        Debug.Log("Applied cheat master data to running game.");
    }

    private static void MarkDirty()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.IsValid())
        {
            EditorSceneManager.MarkSceneDirty(scene);
        }
    }
}
#endif
