#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class GlueFactoryUpgradeMasterDataWindow : EditorWindow
{
    private GlueUpgradeDefinition definition;
    private SerializedObject serializedDefinition;
    private SerializedProperty upgradesProperty;
    private ReorderableList reorderableList;
    private Vector2 scroll;

    [MenuItem("GlueFactory/Open Upgrade Master Data")]
    public static void OpenWindow()
    {
        var window = GetWindow<GlueFactoryUpgradeMasterDataWindow>("Upgrade Master Data");
        window.minSize = new Vector2(680, 420);
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
        if (definition == null)
        {
            serializedDefinition = null;
            upgradesProperty = null;
            reorderableList = null;
            return;
        }

        serializedDefinition = new SerializedObject(definition);
        upgradesProperty = serializedDefinition.FindProperty("upgrades");
        BuildList();
    }

    private void OnGUI()
    {
        DrawHeader();
        if (definition == null || serializedDefinition == null || upgradesProperty == null)
        {
            EditorGUILayout.HelpBox("No GlueUpgradeDefinition found. Generate defaults first.", MessageType.Warning);
            if (GUILayout.Button("Generate Editable Default Upgrade List", GUILayout.Height(28)))
            {
                GlueFactoryUpgradeListTools.GenerateEditableDefaultUpgradeList();
                RefreshBinding();
            }
            return;
        }

        serializedDefinition.Update();
        DrawToolbar();

        scroll = EditorGUILayout.BeginScrollView(scroll);
        reorderableList?.DoLayoutList();
        EditorGUILayout.EndScrollView();

        serializedDefinition.ApplyModifiedProperties();
    }

    private void DrawHeader()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Glue Factory Upgrade Master Data", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Source", definition == null ? "<None>" : definition.name);
        EditorGUILayout.EndVertical();
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add New", GUILayout.Height(26)))
        {
            AddUpgrade();
        }
        if (GUILayout.Button("Ensure Defaults", GUILayout.Height(26)))
        {
            Undo.RecordObject(definition, "Ensure Upgrade Defaults");
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
        EditorGUILayout.Space(6);
    }

    private void BuildList()
    {
        reorderableList = new ReorderableList(serializedDefinition, upgradesProperty, true, true, true, true);
        reorderableList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Upgrade Entries");
        reorderableList.elementHeight = 250f;
        reorderableList.drawElementCallback = (rect, index, _, _) => DrawElement(rect, index);
        reorderableList.onAddCallback = _ => AddUpgrade();
        reorderableList.onRemoveCallback = list => RemoveUpgrade(list.index);
    }

    private void DrawElement(Rect rect, int index)
    {
        var item = upgradesProperty.GetArrayElementAtIndex(index);
        if (item == null)
        {
            return;
        }

        rect.y += 2;
        var line = new Rect(rect.x, rect.y, rect.width, 18);
        var idProp = item.FindPropertyRelative("upgradeId");
        EditorGUI.LabelField(line, $"#{index + 1}  {idProp.stringValue}", EditorStyles.boldLabel);

        line.y += 20;
        EditorGUI.PropertyField(line, idProp, new GUIContent("Upgrade Id"));
        line.y += 20;
        EditorGUI.PropertyField(line, item.FindPropertyRelative("displayName"), new GUIContent("Display Name"));
        line.y += 20;
        EditorGUI.PropertyField(line, item.FindPropertyRelative("description"), new GUIContent("Description"));
        line.y += 20;
        EditorGUI.PropertyField(line, item.FindPropertyRelative("maxLevel"), new GUIContent("Max Level"));
        line.y += 20;
        EditorGUI.PropertyField(line, item.FindPropertyRelative("baseCost"), new GUIContent("Base Cost"));
        line.y += 20;
        EditorGUI.PropertyField(line, item.FindPropertyRelative("growth"), new GUIContent("Growth"));
        line.y += 20;
        EditorGUI.PropertyField(line, item.FindPropertyRelative("effectStartValue"), new GUIContent("Effect Start Value"));
        line.y += 20;
        EditorGUI.PropertyField(line, item.FindPropertyRelative("effectPerLevel"), new GUIContent("Effect Per Level"));
        line.y += 20;
        EditorGUI.PropertyField(line, item.FindPropertyRelative("effectUnit"), new GUIContent("Effect Unit"));
        line.y += 20;
        EditorGUI.PropertyField(line, item.FindPropertyRelative("effectAsPercent"), new GUIContent("Effect As Percent"));
    }

    private void AddUpgrade()
    {
        Undo.RecordObject(definition, "Add Upgrade Entry");
        var list = definition.Upgrades;
        var id = "upgrade_" + list.Count;
        var entry = new GlueUpgradeDefinition.UpgradeEntry();
        entry.SetData(id, "New Upgrade", "Description", 10, 100, 2);
        list.Add(entry);
        EditorUtility.SetDirty(definition);
        MarkDirty();
        RefreshBinding();
    }

    private void RemoveUpgrade(int index)
    {
        var list = definition.Upgrades;
        if (index < 0 || index >= list.Count)
        {
            return;
        }
        Undo.RecordObject(definition, "Remove Upgrade Entry");
        list.RemoveAt(index);
        EditorUtility.SetDirty(definition);
        MarkDirty();
        RefreshBinding();
    }

    private void ApplyToRunningGame()
    {
        serializedDefinition.ApplyModifiedProperties();
        var game = FindFirstObjectByType<GlueFactoryGameManager>();
        if (game == null)
        {
            Debug.LogWarning("Apply failed: GlueFactoryGameManager not found.");
            return;
        }
        definition.ApplyTo(game.Config);
        game.OnUpgradeConfigChanged();
        Debug.Log("Applied upgrade master data to running game.");
    }

    private static GlueUpgradeDefinition FindDefinition()
    {
        var managers = GameObject.Find("GlueFactoryManagers");
        if (managers != null)
        {
            var fromManagers = managers.GetComponentInChildren<GlueUpgradeDefinition>(true);
            if (fromManagers != null)
            {
                return fromManagers;
            }
        }

        var root = GameObject.Find("GlueUpgradeDefaults");
        if (root != null)
        {
            return root.GetComponent<GlueUpgradeDefinition>();
        }

        return FindFirstObjectByType<GlueUpgradeDefinition>(FindObjectsInactive.Include);
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
